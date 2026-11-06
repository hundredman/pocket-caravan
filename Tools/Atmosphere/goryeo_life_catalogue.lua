-- Rendering cells only, exported from whole approved frames; runtime uses whole sprites.
local root=app.params.repo;local folder=root..'/ArtReview/Approved/GoryeoAtmosphereA7/';local out=root..'/Assets/PocketCaravan/Resources/Goryeo/VillageLife/Cells/'
for _,kind in ipairs({'chicken','dog'})do for f=0,1 do
 local s=app.open(folder..kind..'_'..f..'.png')
 for y=0,1 do for x=0,1 do local d=Sprite(16,16,ColorMode.RGB);d.cels[1].image:drawImage(s.cels[1].image,Point(-x*16,-y*16));d:saveAs(out..kind..'_'..f..'_'..x..'_'..y..'.png');d:close()end end
 s:close()
end end
