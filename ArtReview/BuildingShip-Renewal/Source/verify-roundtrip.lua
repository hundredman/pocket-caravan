local root=app.params.root
local temporary=app.params.temporary
for _,a in ipairs(dofile(root.."/Source/specs.lua"))do
 local s=app.open(root.."/Native/"..a.name..".aseprite")
 assert(s.width==a.w and s.height==a.h)
 s:saveCopyAs(temporary.."/"..a.name..".png");s:close()
end
print("ASEPRITE_ROUNDTRIP 10")
