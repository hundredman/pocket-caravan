-- Author intended modular canopy blocks from WHOLE plant instances.
-- Internal render boundaries partition the resulting field, not plant objects.
local project=app.params.project
local root=project.."/ArtReview/RiceField-Tiles-v1"
local source=app.open(project.."/ArtReview/RipeRice-v1/rice-ripe.png")
local plant=source.cels[1].image
local function field(cells,pitch)
 local im=Image(cells*16+32,cells*16+48,ColorMode.RGB)
 for y=0,cells*16-1,pitch do for x=0,cells*16-1,pitch do
  local row=math.floor(y/pitch)
  local px=16+x+math.floor(pitch/2)+(row%2)*4-16
  local py=32+y+pitch-1-32
  im:drawImage(plant,Point(px,py))
 end end
 return im
end
local composite=field(18,16)
local master=Sprite(composite.width,composite.height,ColorMode.RGB)
master.cels[1].image=composite;master.layers[1].name="Whole rice plants, staggered and root-row sorted"
master:saveAs(root.."/Source/field-master.aseprite");master:saveCopyAs(root.."/source-field.png");master:close()
local xs={0,112,208,320};local ys={0,128,224,336}
local names={{"north-west","north","north-east"},{"west","center","east"},{"south-west","south","south-east"}}
for gy=1,3 do for gx=1,3 do
 local w,h=xs[gx+1]-xs[gx],ys[gy+1]-ys[gy]
 local s=Sprite(w,h,ColorMode.RGB);s.layers[1].name="Purpose-made field module: "..names[gy][gx]
 local im=s.cels[1].image
 for y=0,h-1 do for x=0,w-1 do im:drawPixel(x,y,composite:getPixel(xs[gx]+x,ys[gy]+y))end end
 s:saveAs(root.."/Native/"..names[gy][gx]..".aseprite")
 s:saveCopyAs(root.."/Native/"..names[gy][gx]..".png");s:close()
end end
local function standalone(name,pitch)
 local im=field(12,pitch);local s=Sprite(im.width,im.height,ColorMode.RGB);s.cels[1].image=im
 s:saveCopyAs(root.."/"..name..".png");s:close()
end
standalone("dense-reference",16);standalone("sparse-reference",24)
source:close();print("RICE_FIELD 9 native modules, whole-plant16px pitch, 6x6-cell logical blocks")
