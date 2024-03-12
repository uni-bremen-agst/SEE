import {Card, CardContent, Stack } from "@mui/material";
import ServerListItem from "./ServerListItem";
import { useContext, useEffect, useState } from "react";
import Server from "../types/Server";
import { AuthContext } from "../contexts/AuthContext";

function ServerList() {
  const {axiosInstance} = useContext(AuthContext);

  const [servers, setServers] = useState<Server[]>([]);

  useEffect(() => {
    let isApiSubscribed = true;
    const fetchServers = setInterval(() => {
      axiosInstance.get("/server/all").then(
        (response) => setServers(response.data)
      )
    }, 30000);

    if(isApiSubscribed){
      axiosInstance.get("/server/all").then(
        (response) => setServers(response.data)
      )
    }
    return () => {
      isApiSubscribed = false;
      clearInterval(fetchServers);
    }
  }, [])
  
  return (
    <Card elevation={0} sx={{margin: "2em 0 1em 0", maxHeight: "calc(100% - 150px)", overflow: "auto"}}>
    <CardContent>
      <Stack direction="column" spacing={2}>
          {
            servers && servers.length > 0 ?
              servers.map(
                (server) => <ServerListItem server={server} key={server.id}/>
              ) :
              <></>
          }
      </Stack>
    </CardContent>
    </Card>
  )
}

export default ServerList;