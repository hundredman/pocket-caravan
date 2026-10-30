-- Author intended modular canopy blocks from WHOLE plant instances.
-- Internal render boundaries partition the resulting field, not plant objects.
local project=app.params.project
local root=project.."/ArtReview/RiceField-Tiles-"..(app.params.revision or "v1")
local source=app.open(project.."/"..(app.params.plant or "ArtReview/RipeRice-v1/rice-ripe.png"))
local plant=source.cels[1].image
local pitch=tonumber(app.params.pitch or "16")
local blockCells=tonumber(app.params.blockCells or "6")
local block=blockCells*16
assert(block%pitch==0 and (block/pitch)%2==0,"Block must repeat the stagger phase")
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
local composite=field(blockCells*3,pitch)
local master=Sprite(composite.width,composite.height,ColorMode.RGB)
master.cels[1].image=composite;master.layers[1].name="Whole rice plants, staggered and root-row sorted"
master:saveAs(root.."/Source/field-master.aseprite");master:saveCopyAs(root.."/source-field.png");master:close()
local xs={0,block+16,block*2+16,block*3+32};local ys={0,block+32,block*2+32,block*3+48}
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
 local im=field(blockCells*2,pitch);local s=Sprite(im.width,im.height,ColorMode.RGB);s.cels[1].image=im
 s:saveCopyAs(root.."/"..name..".png");s:close()
end
standalone("dense-reference",pitch);standalone("sparse-reference",tonumber(app.params.referencePitch or "24"))
source:close();print("RICE_FIELD 9 native modules; pitch="..pitch.."; blockCells="..blockCells)
