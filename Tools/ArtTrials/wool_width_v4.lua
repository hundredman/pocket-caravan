-- User-requested offline whole-icon horizontal refinement. Canvas/height remain 16px.
local root=app.params.repo;local src=app.open(root..'/ArtReview/ItemIcons-v3-20261104/Native/transit_wool.aseprite');local im=src.cels[1].image;local pc=app.pixelColor
local x0,x1=16,0
for p in im:pixels()do if pc.rgbaA(p())>0 then x0=math.min(x0,p.x);x1=math.max(x1,p.x)end end
local width=x1-x0+1;local target=width-2;local out=Sprite(16,16,ColorMode.RGB);local di=out.cels[1].image;local ox=math.floor((16-target)/2)
for y=0,15 do for x=0,target-1 do local sx=x0+math.min(width-1,math.floor((x+.5)*width/target));di:drawPixel(ox+x,y,im:getPixel(sx,y))end end
local folder=root..'/ArtReview/WoolWidth-v4-20261104/Native/';out:saveAs(folder..'transit_wool.aseprite');out:saveAs(folder..'transit_wool.png');out:close();src:close()
