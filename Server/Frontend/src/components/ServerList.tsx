import { Card, CardContent, Grid } from "@mui/material";
import ServerListItem from "./ServerListItem";
import { useContext, useEffect, useState } from "react";
import Server from "../types/Server";
import { AuthContext } from "../contexts/AuthContext";
import AppUtils from "../utils/AppUtils";

function ServerList() {
  const { axiosInstance } = useContext(AuthContext);

  const [servers, setServers] = useState<Server[]>([]);

  async function refreshData() {
    await axiosInstance.get("/server/all").then(
      (response) => {
        setServers(response.data)
        AppUtils.notifyOnline();
      }
    ).catch(
      () => AppUtils.notifyOffline()
    );
  }

  useEffect(() => {
    refreshData();
    const refreshInterval = setInterval(() => refreshData(), 10000);
    return () => {
      clearInterval(refreshInterval);
    }
  }, [])

  return (
    <Card elevation={0} sx={{ margin: "2em 0 1em 0", maxHeight: "calc(100% - 150px)", overflow: "auto" }}>
      <CardContent>
        <Grid container spacing={2}>
          {
            servers && servers.length ? servers.map((server) =>
              <Grid key={server.id} item xs={12} sm={12} md={6} lg={6}>
                <ServerListItem server={server} key={server.id} />
              </Grid>
            ) : <></>
          }
        </Grid>
      </CardContent>
    </Card>
  )
}

export default ServerList;
