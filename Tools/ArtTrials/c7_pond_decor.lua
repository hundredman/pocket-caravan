-- Native source authoring. AI transparency haze is discarded at alpha224;
-- the complete pads/blossom are uniformly sampled, never cut from old art.
local root=assert(app.params.root);local pc=app.pixelColor
local finish=app.params.finish=='true'
for _,name in ipairs({'leaves','flower'}) do
 if not finish then
  local src=app.open(root..'/AI/'..name..'.png');local im=src.cels[1].image
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for y=0,im.height-1 do for x=0,im.width-1 do
   if pc.rgbaA(im:getPixel(x,y))>=224 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y) end
  end end
  local scale=math.min(14/(x1-x0+1),10/(y1-y0+1))
  local w,h=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
  local s=Sprite(16,16,ColorMode.RGB);local dst=s.cels[1].image
  for y=0,h-1 do for x=0,w-1 do
   local sx=x0+math.min(x1-x0,math.floor((x+.5)/w*(x1-x0+1)))
   local sy=y0+math.min(y1-y0,math.floor((y+.5)/h*(y1-y0+1)))
   local v=im:getPixel(sx,sy)
   if pc.rgbaA(v)>=224 then dst:drawPixel(math.floor((16-w)/2)+x,14-h+y,pc.rgba(pc.rgbaR(v),pc.rgbaG(v),pc.rgbaB(v),255)) end
  end end
  s:saveCopyAs(root..'/Prepared/'..name..'-seed.png');s:close();src:close()
 else
  local s=app.open(root..'/Prepared/'..name..'.png');s.layers[1].name='Native pond decoration / complete object / A5 colors'
  s:saveAs(root..'/Native/'..name..'.aseprite');s:saveCopyAs(root..'/Native/'..name..'.png');s:close()
 end
end
