import { Box, Card, CardActionArea, CardContent, Chip, IconButton, Stack, Typography } from "@mui/material";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faClipboard } from '@fortawesome/free-solid-svg-icons'
import { useNavigate } from "react-router";
import Avatar from "./Avatar";
import Server from "../types/Server";
import { enqueueSnackbar } from "notistack";

function getServerStatus(serverStatus: string) {
  switch (serverStatus) {
    case "ONLINE":
      return <Chip color="success" label="Online" />;
    case "OFFLINE":
      return <Chip color="error" label="Offline" />;
    case "ERROR":
      return <Chip color="error" label="ERROR" />;
  }
}

function ServerListItem(props: { server: Server }) {
  const navigate = useNavigate();

  const server = props.server;

  return (
    <Box width={"100%"}>
      <Card sx={{ cursor: "pointer", borderRadius: "25px" }}>
        <CardActionArea onClick={() => navigate('/server#' + server.id)}>
          <CardContent>
            <Stack direction="row" spacing={2}>
              {/* Avatar */}
              <Box width={100} height={100}>
                <Card sx={{ width: "100%", height: "100%" }}>
                  <Avatar width={100} height={100} avatarSeed={server.avatarSeed} avatarColor={server.avatarColor} />
                </Card>
              </Box>
              {/* Details */}
              <Stack direction="column" spacing={1}>
                <Stack direction="row" spacing={1}>
                  <Typography variant="h6">{server.name}</Typography>
                  {getServerStatus(server.status)}
                </Stack>
                {server.status == "ONLINE"
                  ? <Typography>Online since: {new Date(server.startTime * 1000).toLocaleString()}</Typography>
                  : <Typography>Offline since:
                    {server.stopTime
                      ? ` ${new Date(server.stopTime * 1000).toLocaleString()}`
                      : ` ${new Date(server.creationTime * 1000).toLocaleString()}`
                    }
                  </Typography>
                }
                <Stack direction="row" spacing={1}>
                  <Typography>Address:&nbsp;</Typography>
                  <Typography sx={{ fontFamily: "monospace" }}>{server.containerAddress}:{server.containerPort}</Typography>
                  <IconButton
                    size="small"
                    component="span"
                    onMouseDown={(e) => { e.stopPropagation(); e.preventDefault(); }}
                    onClick={(e) => {
                      e.stopPropagation();
                      e.preventDefault();
                      navigator.clipboard.writeText(`${server.containerAddress}:${server.containerPort}`);
                      enqueueSnackbar("Address was copied to clipboard.", { variant: "info" });
                    }}>
                    <FontAwesomeIcon icon={faClipboard} />
                  </IconButton>
                </Stack>
              </Stack>
            </Stack>
          </CardContent>
        </CardActionArea>
      </Card>
    </Box>
  )
}

export default ServerListItem;
