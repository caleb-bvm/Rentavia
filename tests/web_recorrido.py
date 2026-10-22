"""Prueba HTTP del recorrido completo. Usar sobre una base de pruebas."""
import argparse
import json
import os
import secrets
from datetime import datetime, timedelta, timezone
from pathlib import Path
import requests
from bs4 import BeautifulSoup

parser = argparse.ArgumentParser()
parser.add_argument('--url', default='http://127.0.0.1:5295')
args = parser.parse_args()
base = args.url.rstrip('/')
root = Path(__file__).resolve().parents[1]
config = dict(line.split('=', 1) for line in (root/'.env').read_text(encoding='utf-8').splitlines() if '=' in line and not line.startswith('#'))
session = requests.Session()
results = []

def check(condition, name):
    if not condition:
        raise AssertionError(name)
    results.append({'caso': name, 'estado': 'Correcto'})
    print('OK:', name)

def get(path):
    r = session.get(base+path, timeout=30)
    check(r.status_code == 200, 'GET '+path.split('?')[0])
    return r

def submit(path, values, source=None):
    html = get(source or path).text
    doc = BeautifulSoup(html, 'html.parser')
    form = next((f for f in doc.select('form') if (f.get('action') or '').split('?')[0].lower() == path.lower()), None)
    if form is None:
        form = next(f for f in doc.select('form') if f.get('method','').lower()=='post' and 'Salir' not in f.get('action',''))
    data = {i['name']: i.get('value','') for i in form.select('input[name][type=hidden]')}
    data.update(values)
    return session.post(base+path, data=data, timeout=45, allow_redirects=False)

def success(response, name):
    if response.status_code != 302:
        doc = BeautifulSoup(response.text,'html.parser')
        summary = doc.select_one('.validation-summary-errors')
        raise AssertionError(name+': '+(summary.get_text(' ',strip=True) if summary else str(response.status_code)))
    check(True, name)
    return response.headers['Location']

suffix = secrets.token_hex(4)
placa = ('P'+suffix[:3]+'-'+suffix[3:6]).upper()
try:
    anonymous = requests.get(base+'/Reservas',allow_redirects=False,timeout=15)
    check(anonymous.status_code==302 and '/Cuenta/Ingresar' in anonymous.headers['Location'],'Rutas privadas requieren sesión')
    response=submit('/Cuenta/Ingresar',{'Correo':config['ADMIN_EMAIL'],'Clave':config['ADMIN_PASSWORD'],'ReturnUrl':'/'})
    success(response,'Inicio de sesión administrador')
    for path in ['/', '/Vehiculos', '/Clientes', '/Categorias', '/Sucursales', '/Reservas', '/Alquileres', '/Mantenimientos', '/Disponibilidad', '/Recomendaciones', '/Reportes', '/Reportes/Auditoria', '/Usuarios', '/Home/Privacy', '/health']:
        get(path)
    for path in ['/Categorias/Editar','/Sucursales/Editar','/Clientes/Editar','/Vehiculos/Editar','/Reservas/Editar','/Mantenimientos/Crear','/Usuarios/Crear']:
        get(path)
    # Crear y eliminar catálogos no referenciados.
    nombre='PRUEBA-'+suffix
    success(submit('/Categorias/Editar',{'Id':0,'Nombre':nombre,'Descripcion':'Vehículos compactos para ciudad'}),'CRUD: crear categoría')
    row=next(r for r in BeautifulSoup(get('/Categorias').text,'html.parser').select('tbody tr') if nombre in r.get_text())
    category_id=row.select_one('input[name=id]')['value']
    success(submit('/Categorias/Editar',{'Id':category_id,'Nombre':nombre+'-EDITADA','Descripcion':'Descripción actualizada'},'/Categorias/Editar/'+category_id),'CRUD: editar categoría')
    success(submit('/Categorias/Eliminar',{'id':category_id},'/Categorias'),'CRUD: eliminar categoría no referenciada')
    success(submit('/Sucursales/Editar',{'Id':0,'Nombre':nombre,'Direccion':'Colonia Escalón, San Salvador','Telefono':'0000-0000','Activa':'true'}),'CRUD: crear sucursal')
    row=next(r for r in BeautifulSoup(get('/Sucursales').text,'html.parser').select('tbody tr') if nombre in r.get_text())
    branch_id=row.select_one('input[name=id]')['value']
    success(submit('/Sucursales/Editar',{'Id':branch_id,'Nombre':nombre+'-EDITADA','Direccion':'Paseo General Escalón, San Salvador','Activa':'true'},'/Sucursales/Editar/'+branch_id),'CRUD: editar sucursal')
    success(submit('/Sucursales/Eliminar',{'id':branch_id},'/Sucursales'),'CRUD: eliminar sucursal no referenciada')
    client_data={'Id':0,'Nombre':'Prueba','Apellido':suffix,'TipoDocumento':'PASAPORTE','NumeroPasaporte':suffix,'Telefono':'0000-0000','Activo':'true'}
    success(submit('/Clientes/Editar',client_data),'CRUD: crear cliente')
    row=next(r for r in BeautifulSoup(get('/Clientes').text,'html.parser').select('tbody tr') if suffix in r.get_text())
    new_client_id=row.select_one('input[name=id]')['value']
    edit_doc=BeautifulSoup(get('/Clientes/Editar/'+new_client_id).text,'html.parser')
    old_client_version=edit_doc.select_one('#Version')['value']
    client_data.update({'Id':new_client_id,'Nombre':'Prueba editada','Version':old_client_version})
    success(submit('/Clientes/Editar',client_data,'/Clientes/Editar/'+new_client_id),'CRUD: editar cliente')
    r=submit('/Clientes/Editar',client_data,'/Clientes/Editar/'+new_client_id)
    check(r.status_code==200 and 'Otro usuario' in r.text,'Edición con versión anterior rechazada en formulario')
    new_version=BeautifulSoup(get('/Clientes/Editar/'+new_client_id).text,'html.parser').select_one('#Version')['value']
    success(submit('/Clientes/Eliminar',{'id':new_client_id,'version':new_version},'/Clientes'),'CRUD: eliminar cliente no referenciado')
    options=BeautifulSoup(get('/Vehiculos/Editar').text,'html.parser')
    c_id=next(o['value'] for o in options.select('#CategoriaId option') if o.get('value'))
    s_id=next(o['value'] for o in options.select('#SucursalId option') if o.get('value'))
    vehicle_data={'Id':0,'CategoriaId':c_id,'SucursalId':s_id,'Placa':placa,'Marca':'Nissan','Modelo':'Prueba','Anio':2024,'CapacidadPasajeros':5,'CapacidadMaletas':2,'Kilometraje':0,'Transmision':0,'Estado':0,'TarifaDiaria':'40'}
    success(submit('/Vehiculos/Editar',vehicle_data),'CRUD: crear vehículo')
    r=submit('/Vehiculos/Editar',vehicle_data)
    check(r.status_code==200 and 'No se pudo guardar' in r.text,'Placa duplicada rechazada en formulario')
    row=next(r for r in BeautifulSoup(get('/Vehiculos').text,'html.parser').select('tbody tr') if placa in r.get_text())
    new_vehicle_id=row.select_one('input[name=id]')['value']
    old_vehicle_version=BeautifulSoup(get('/Vehiculos/Editar/'+new_vehicle_id).text,'html.parser').select_one('#Version')['value']
    vehicle_data.update({'Id':new_vehicle_id,'Modelo':'Prueba editada','Version':old_vehicle_version})
    success(submit('/Vehiculos/Editar',vehicle_data,'/Vehiculos/Editar/'+new_vehicle_id),'CRUD: editar vehículo')
    new_version=BeautifulSoup(get('/Vehiculos/Editar/'+new_vehicle_id).text,'html.parser').select_one('#Version')['value']
    success(submit('/Vehiculos/Eliminar',{'id':new_vehicle_id,'version':new_version},'/Vehiculos'),'CRUD: eliminar vehículo no referenciado')
    # La flota inicial permite completar un alquiler real, sin alterar datos personales.
    doc=BeautifulSoup(get('/Reservas/Editar').text,'html.parser')
    client_id=next(o['value'] for o in doc.select('#ClienteId option') if o.get('value'))
    vehicle_id=next(o['value'] for o in doc.select('#VehiculoId option') if o.get('value'))
    local=datetime.now(timezone(timedelta(hours=-6))).replace(tzinfo=None)
    start=(local+timedelta(minutes=1)).strftime('%Y-%m-%dT%H:%M')
    end=(local+timedelta(days=1,minutes=1)).strftime('%Y-%m-%dT%H:%M')
    success(submit('/Reservas/Editar',{'Id':0,'ClienteId':client_id,'VehiculoId':vehicle_id,'Inicio':start,'Fin':end}),'Crear reserva desde formulario')
    doc=BeautifulSoup(get('/Reservas').text,'html.parser')
    entrega=next(a['href'] for a in doc.select('a') if '/Alquileres/Entregar' in a.get('href',''))
    entrega_doc=BeautifulSoup(get(entrega).text,'html.parser')
    km=int(entrega_doc.select_one('#Kilometraje')['value'])
    detalle=success(submit('/Alquileres/Entregar',{'Kilometraje':km,'Combustible':100,'Observaciones':'Inspección de prueba'},entrega),'Registrar entrega desde formulario')
    rental_id=detalle.rstrip('/').split('/')[-1]
    # Rechazar una escritura sin el token antifalsificación.
    r=session.post(base+'/Alquileres/Pago',data={'AlquilerId':rental_id,'Monto':'1','Metodo':'Efectivo'},timeout=15)
    check(r.status_code==400,'Antifalsificación rechaza POST sin token')
    success(submit('/Alquileres/Pago',{'AlquilerId':rental_id,'Monto':'10','Metodo':'Efectivo','Referencia':'REF-'+suffix},detalle),'Registrar pago interno')
    get(detalle)
    success(submit('/Alquileres/Devolver',{'Id':rental_id,'Kilometraje':km+50,'Combustible':80,'Observaciones':'Vehículo inspeccionado','Cargo':'5','Concepto':'Limpieza adicional'},'/Alquileres/Devolver/'+rental_id),'Registrar devolución y cerrar alquiler')
    check('Cerrado' in get(detalle).text,'Alquiler cerrado visible')
    r=submit('/Recomendaciones',{'Inicio':start,'Fin':end,'Pasajeros':4,'Maletas':2,'PresupuestoDiario':100,'Preferencias':'Viaje familiar de fin de semana'})
    check(r.status_code==200 and 'Reglas (sin IA)' in BeautifulSoup(r.text,'html.parser').get_text(),'Respaldo sin clave identificado en la pantalla')
    # Un empleado no accede a la administración.
    empleado='empleado.'+suffix+'@rentavia.test'
    employee_password='Aa1!'+secrets.token_hex(16)
    success(submit('/Usuarios/Crear',{'Correo':empleado,'Clave':employee_password,'Rol':'Empleado'}),'Crear cuenta empleado')
    success(submit('/Cuenta/Salir',{},'/'),'Cerrar sesión')
    success(submit('/Cuenta/Ingresar',{'Correo':empleado,'Clave':employee_password,'ReturnUrl':'/'}),'Inicio de sesión empleado')
    r=session.get(base+'/Usuarios',timeout=15)
    check(r.status_code==403,'Empleado sin acceso a usuarios')
    r=session.post(base+'/Categorias/Eliminar',data={'id':1},timeout=15)
    check(r.status_code==403 or (r.status_code==302 and 'SinPermiso' in r.headers.get('Location','')),'Empleado no puede eliminar categorías')
finally:
    output=root/'docs/evidencias/Recorrido-web.json'
    output.write_text(json.dumps({'fechaUtc':datetime.now(timezone.utc).isoformat(),'url':base,'pruebas':results},ensure_ascii=False,indent=2),encoding='utf-8')
print('Recorrido completado:',len(results),'comprobaciones.')
