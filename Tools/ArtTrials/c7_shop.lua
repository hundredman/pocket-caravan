-- Whole-object authoring only; no runtime transformation or partial sprite reuse.
local root=assert(app.params.root);local pc=app.pixelColor
if app.params.finish~='true'then
 local s=app.open(root..'/AI/shop.png');local im=s.cels[1].image
 local x0,y0,x1,y1=im.width,im.height,-1,-1
 for y=0,im.height-1 do for x=0,im.width-1 do if pc.rgbaA(im:getPixel(x,y))>=224 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)end end end
 assert(x1>=x0,'No opaque shop')
 local scale=math.min(92/(x1-x0+1),60/(y1-y0+1))
 local w,h=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
 local out=Sprite(96,64,ColorMode.RGB);local dst=out.cels[1].image
 local ox,oy=math.floor((96-w)/2),63-h
 for y=0,h-1 do for x=0,w-1 do
  local sx=x0+math.min(x1-x0,math.floor((x+.5)/w*(x1-x0+1)))
  local sy=y0+math.min(y1-y0,math.floor((y+.5)/h*(y1-y0+1)))
  local v=im:getPixel(sx,sy)
  if pc.rgbaA(v)>=224 then dst:drawPixel(ox+x,oy+y,pc.rgba(pc.rgbaR(v),pc.rgbaG(v),pc.rgbaB(v),255))end
 end end
 out.layers[1].name='Whole AI shop on native 96x64 grid'
 out:saveCopyAs(root..'/Prepared/shop-seed.png');out:close();s:close()
 print('Whole shop '..w..'x'..h..' at '..ox..','..oy)
else
 local s=app.open(root..'/Prepared/shop.png');s.layers[1].name='A5 candidate colors / complete shop'
 local base=s.cels[1].image;local layer=s:newLayer();layer.name='Native one pixel exterior contour'
 local im=Image(96,64,ColorMode.RGB)
 for y=0,63 do for x=0,95 do
  if pc.rgbaA(base:getPixel(x,y))>0 then
   local edge=false
   for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}})do
    local xx,yy=x+d[1],y+d[2]
    if xx<0 or yy<0 or xx>=96 or yy>=64 or pc.rgbaA(base:getPixel(xx,yy))==0 then edge=true end
   end
   if edge then im:drawPixel(x,y,pc.rgba(46,36,29,255))end
  end
 end end
 s:newCel(layer,1,im,Point(0,0))
 s:saveAs(root..'/Native/shop.aseprite');s:saveCopyAs(root..'/Native/shop.png');s:close()
end
