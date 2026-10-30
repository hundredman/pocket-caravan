-- Local native touch-up of complete editable sprites. No generation/resizing.
local root=assert(app.params.root);local base=assert(app.params.base);local pc=app.pixelColor
local function c(h)return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
for _,name in ipairs({'harvest','celadon'})do
 local s=app.open(base..'/ArtReview/C7-WorkshopProps-v2-20261027/Native/'..name..'.aseprite')
 local im=Image(s.spec);im:drawSprite(s,1)
 local function put(x,y,h)im:drawPixel(x,y,c(h))end
 local function clear(x0,y0,x1,y1)for y=y0,y1 do for x=x0,x1 do im:drawPixel(x,y,pc.rgba(0,0,0,0))end end end
 local function row(y,x0,x1,h)for x=x0,x1 do put(x,y,h)end end
 if name=='harvest'then
  -- Left bundle: fuller cut-straw body under the existing small grain head.
  clear(8,17,14,28)
  for y=17,19 do row(y,9,12,'2e241d');row(y,10,11,'daa34a')end
  row(20,9,12,'2e241d');row(20,10,11,'f0c365');row(21,9,12,'2e241d')
  for y=22,26 do
   local x0,x1=y<24 and 9 or 8,y<24 and 12 or 13
   row(y,x0,x1,'2e241d');row(y,x0+1,x1-1,'b78137');put(10,y,'f0c365');put(x1-1,y,'daa34a')
  end
  row(27,9,12,'2e241d')
  -- Central bundle: add thickness on its exposed left side, leaving the sack.
  for y=16,17 do row(y,14,18,'2e241d');row(y,15,17,'daa34a');put(16,y,'f0c365')end
  for y=18,21 do row(y,14,17,'2e241d');row(y,15,16,'b78137');put(15,y,'daa34a')end
  row(20,15,16,'f0c365')
  -- Right bundle's exposed stalks, above/right of the unchanged sack.
  for y=17,19 do put(26,y,'daa34a');put(27,y,'2e241d')end
 else
  -- Remove the six-pixel dark smudge, draw one clean rim and narrow neck.
  clear(11,7,18,12)
  row(7,12,16,'2e241d')
  row(8,12,16,'2e241d');row(8,13,15,'304d47');put(15,8,'86a28b')
  for y=9,12 do row(y,13,15,'2e241d');put(14,y,'86a28b')end
 end
 -- Preserve the hidden editable previous layers, add a complete corrected layer.
 for _,l in ipairs(s.layers)do l.isVisible=false end
 local corrected=s:newLayer();corrected.name=name=='harvest' and 'Native fuller straw bundles, small goods preserved' or 'Native clean mouth rim and neck';s:newCel(corrected,1,im,Point(0,0))
 -- Outline only new exposed edges; previous exterior pixels are already ink.
 local edge=Image(48,32,ColorMode.RGB)
 for y=0,31 do for x=0,47 do if pc.rgbaA(im:getPixel(x,y))>0 then
  for _,d in ipairs({{1,0},{-1,0},{0,1},{0,-1}})do local xx,yy=x+d[1],y+d[2]
   if xx<0 or yy<0 or xx>=48 or yy>=32 or pc.rgbaA(im:getPixel(xx,yy))==0 then edge:drawPixel(x,y,c('2e241d'));break end
  end
 end end end
 local outline=s:newLayer();outline.name='Native exterior contour';s:newCel(outline,1,edge,Point(0,0))
 s:saveAs(root..'/Native/'..name..'.aseprite');s:saveCopyAs(root..'/Native/'..name..'.png');s:close()
end
