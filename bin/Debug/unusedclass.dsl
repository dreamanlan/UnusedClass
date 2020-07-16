/*
collect("class", "field");
addclasses(file1,file2,...);
removeclasses(file1,file2,...);

collecter{
    base("")interface("IJceMessage","ICs2LuaJceMessage")include("")match("")except("")exceptmatch("");
};

marker{
    include("")match("")except("")exceptmatch("");
};
*/
collect("class");

marker{
    match(".*")except("MessageDefine.MessageEnum2Object", "MessageDefine.Cs2LuaMessageEnum2Object");
};

log("__Error");
log("Cs2LuaObjectPool_T");
log("Cs2LuaSimpleObjectPoolEx_T");