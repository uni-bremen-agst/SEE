import { Box, Button, Card, CardActionArea, CardContent, Container, Stack, TextField, Typography } from "@mui/material";
import Header from "../components/Header";
import { grey } from "@mui/material/colors";
import { useNavigate } from "react-router";
import Avatar from "../components/Avatar";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowLeft, faRepeat, faX } from "@fortawesome/free-solid-svg-icons";
import { useContext, useState } from "react";
import { MuiFileInput } from 'mui-file-input';
import { AuthContext } from "../contexts/AuthContext";

function getRandomColor(){
  const red = (Math.floor(Math.random() * 150) + 100).toString();
  const green = (Math.floor(Math.random() * 150) + 100).toString();
  const blue = (Math.floor(Math.random() * 150) + 100).toString();

  return `rgb(${red}, ${green}, ${blue})`;
}

function getRandomSeed(){
  let avatarSeed = "";
  for(let i = 0; i < 18; i++){
      avatarSeed = avatarSeed + Math.round(Math.random()).toString();
  }
  return avatarSeed;
}

function CreateServerView() {
    const {axiosInstance} = useContext(AuthContext);

    const navigate = useNavigate();

    const [name, setName] = useState<string>("");
    const [serverPassword, setServerPassword] = useState<string>("");

    const [code, setCode] = useState<File|null>(null);
    const [gxl, setGxl] = useState<File|null>(null);
    const [csv, setCsv] = useState<File|null>(null);
    const [configuration, setConfiguration] = useState<File|null>(null);
    const [solution, setSolution] = useState<File|null>(null);

    const [avatarSeed, setAvatarSeed] = useState(getRandomSeed());
    const [avatarColor, setAvatarColor] = useState(getRandomColor());
    const [displayReloadIcon, setDisplayReloadIcon] = useState(false);

    const [errors, setErrors] = useState(new Map<string, string>());

    async function createServer(){
      const createServerResponse = await axiosInstance.post("/server/create", {name: name, serverPassword: serverPassword, avatarSeed: avatarSeed, avatarColor: avatarColor});
      if(!createServerResponse) {return;}

      let codeResponse = null;
      let gxlRespose = null;
      let csvRespose = null;
      let configurationResponse = null;
      let solutionResponse = null;

      if(code){
        const form = new FormData();
        form.append("id", createServerResponse.data.id);
        form.append("fileType", "SOURCE");
        form.append("file", code);
        codeResponse = await axiosInstance.post("/server/addFile", form)
      }
      if(gxl){
        const form = new FormData();
        form.append("id", createServerResponse.data.id);
        form.append("fileType", "GXL");
        form.append("file", gxl);
        gxlRespose = await axiosInstance.post("/server/addFile", form)
      }
      if(csv){
        const form = new FormData();
        form.append("id", createServerResponse.data.id);
        form.append("fileType", "CSV");
        form.append("file", csv);
        csvRespose = await axiosInstance.post("/server/addFile", form)
      }
      if(configuration){
        const form = new FormData();
        form.append("id", createServerResponse.data.id);
        form.append("fileType", "CONFIG");
        form.append("file", configuration);
        configurationResponse = await axiosInstance.post("/server/addFile", form)
      }
      if(solution){
        const form = new FormData();
        form.append("id", createServerResponse.data.id);
        form.append("fileType", "SOLUTION");
        form.append("file", solution);
        solutionResponse = await axiosInstance.post("/server/addFile", form)
      }

      if(
        !(code && !codeResponse 
          || gxl && !gxlRespose 
          || csv && !csvRespose 
          || configuration && !configurationResponse 
          || solution && !solutionResponse)
      ) {
        axiosInstance.put("/server/startServer", {}, {params: {id: createServerResponse.data.id}})
      }
      navigate('/', {replace: true});
    }

    return (
      <Container sx={{padding: "3em", height:"100vh"}}>
        <Header/>
        <Card sx={{marginTop: "2em", borderRadius: "25px", height: "calc(100% - 100px)", overflow: "auto"}}>
          <CardContent sx={{height: "calc(100% - 3em)"}}>
            <Stack direction="column" spacing={2} height={"100%"}>
              <Typography variant="h4"><Box display={"inline"} sx={{"&:hover" : {cursor: "pointer"}}}><FontAwesomeIcon icon={faArrowLeft} onClick={() => navigate(-1)}/></Box> Gameserver erstellen</Typography>
              <Stack direction="row" spacing={2}>
                <Stack direction="column" flexGrow={1}>
                  <Typography variant="h6">Gameservereinstellungen:</Typography>
                  <TextField error={!!errors.get("name")} helperText={errors.get("name")} value={name} onChange={(e) => setName(e.target.value)} label="Name" variant="standard"/>
                  <TextField value={serverPassword} onChange={(e) => setServerPassword(e.target.value)} label="Serverpasswort" variant="standard"/>
                </Stack>
                <Stack direction="column" >
                  <Typography variant="h6">Server-Bild:</Typography>
                  <Box width={140} height={140} paddingTop="1.5em">
                        <Card sx={{width: "100%", height: "100%"}}>
                          <CardActionArea onMouseEnter={() => setDisplayReloadIcon(true)} onMouseLeave={() => setDisplayReloadIcon(false)} onClick={() => {setAvatarSeed(getRandomSeed()); setAvatarColor(getRandomColor());}}>
                            <Box 
                              visibility={displayReloadIcon? "visible" : "hidden"} 
                              position="absolute" color={grey[500]} 
                              display="flex" 
                              width={140}
                              height={140} 
                              justifyContent="center"
                              sx={{mixBlendMode : "difference"}}
                            >
                              <Stack direction="column" justifyContent="center">
                                <FontAwesomeIcon icon={faRepeat} size="4x"/>
                              </Stack>
                            </Box>
                            <Avatar width={140} height={140} avatarSeed={avatarSeed} avatarColor={avatarColor}/>
                          </CardActionArea>
                        </Card>
                      </Box>
                </Stack>
                
              </Stack>
              <Typography variant="h6">Dateien:</Typography>
              <Card sx={{borderRadius: "0px", flexGrow: 1, overflow: "auto", minHeight: "100px"}} elevation={0}>
                <MuiFileInput label="Code" placeholder="Code hochladen.." variant="standard" fullWidth value={code} onChange={(value) => setCode(value)} clearIconButtonProps={{title: "Entfernen", children: <FontAwesomeIcon icon={faX}/>}} inputProps={{accept: '.zip'}}/>
                <MuiFileInput label="GXL" placeholder="GXL hochladen.." variant="standard" fullWidth value={gxl} onChange={(value) => setGxl(value)} clearIconButtonProps={{title: "Entfernen", children: <FontAwesomeIcon icon={faX}/>}} inputProps={{accept: '.gxl'}}/>
                <MuiFileInput label="CSV" placeholder="CSV hochladen.." variant="standard" fullWidth value={csv} onChange={(value) => setCsv(value)} clearIconButtonProps={{title: "Entfernen", children: <FontAwesomeIcon icon={faX}/>}} inputProps={{accept: '.csv'}}/>
                <MuiFileInput label="Config" placeholder="Config hochladen.." variant="standard" fullWidth value={configuration} onChange={(value) => setConfiguration(value)} clearIconButtonProps={{title: "Entfernen", children: <FontAwesomeIcon icon={faX}/>}} inputProps={{accept: '.cfg'}}/>
                <MuiFileInput label="Solution" placeholder="Solution hochladen.." variant="standard" fullWidth value={solution} onChange={(value) => setSolution(value)} clearIconButtonProps={{title: "Entfernen", children: <FontAwesomeIcon icon={faX}/>}}/>
              </Card> 
              <Stack justifyContent="end" direction="row" spacing={2}>
                <Button variant="contained" color="secondary" sx={{borderRadius:"25px"}} onClick={() => navigate('/')}>
                    Abbrechen
                </Button>
                <Button variant="contained" sx={{borderRadius:"25px"}} onClick={() => {
                  setErrors(new Map<string, string>());
                  let tempErrorsList = new Map<string, string>();
                  if(!name){
                    tempErrorsList.set('name', "Name muss angegeben werden.");
                  }
                  if(tempErrorsList.size > 0){
                    setErrors(tempErrorsList);
                  } else {
                    createServer()
                  }
                }}>
                    Erstellen
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      </Container>
    )
  }

  export default CreateServerView;