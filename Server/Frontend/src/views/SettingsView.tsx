import { Box, Button, Card, CardContent, Container, IconButton, List, ListItem, ListItemText, Modal, Stack, TextField, Typography } from "@mui/material";
import Header from "../components/Header";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowLeft, faCrown, faRepeat, faUserMinus } from "@fortawesome/free-solid-svg-icons";
import { grey, yellow } from "@mui/material/colors";
import { useContext, useEffect, useState } from "react";
import User from "../types/User";
import { Navigate, useNavigate } from "react-router";
import { AuthContext } from "../contexts/AuthContext";

const modalStyle = {
  position: 'absolute',
  top: '50%',
  left: '50%',
  transform: 'translate(-50%, -50%)',
  maxWidth: "1200px",
  minWidth: "400px",
  bgcolor: 'background.paper',
  borderRadius: "25px",
  boxShadow: 24,
  p: 4,
};

function generateRandomPassword(){
  const password = [];
  for(let i = 0; i < 16; i++){
    password.push(String.fromCharCode(Math.random()*86+40));
  }
  return password.join("");
}

function SettingsView() {
    const {user, axiosInstance} = useContext(AuthContext);

    const navigate = useNavigate();

    const [addUserModalOpen, setAddUserModalOpen] = useState(false);
    const [removeUserModalOpen, setRemoveUserModalOpen] = useState(false);
    const [promoteDemoteUserModalOpen, setPromoteDemoteUserModalOpen] = useState(false);
    const [users, setUsers] = useState<User[]>([]);
    const [selectedUser, setSelectedUser] = useState<User | null>(null);
    const [addUserUsername, setAddUserUsername] = useState("");
    const [addUserPassword, setAddUserPassword] = useState("");

    async function addUser() {
      if(!addUserUsername || !addUserPassword){
        return;
      }
      const addUserResponse = await axiosInstance.post('/user/create', {username: addUserUsername, password: addUserPassword, role: 'ROLE_USER'});
      if(addUserResponse){
        setAddUserModalOpen(false);
        axiosInstance.get("/user/all").then(
          (response) => setUsers(response.data)
        )
      }      
    }

    async function removeUser() {
      if(!selectedUser){
        return;
      }
      const addUserResponse = await axiosInstance.delete('/user/delete', {params: {username: selectedUser.username}});
      if(addUserResponse){
        setRemoveUserModalOpen(false);
        axiosInstance.get("/user/all").then(
          (response) => setUsers(response.data)
        )
      }      
    }

    async function promoteUser() {
      if(!selectedUser){
        return;
      }
      const promoteUserResponse = await axiosInstance.post('/user/addRoleToUser', {}, {params: {username: selectedUser.username, role: "ROLE_ADMIN"}});
      if(promoteUserResponse){
        setPromoteDemoteUserModalOpen(false);
        axiosInstance.get("/user/all").then(
          (response) => setUsers(response.data)
        )
      }      
    }

    async function demoteUser() {
      if(!selectedUser){
        return;
      }
      const demoteUserResponse = await axiosInstance.delete('/user/removeRoleFromUser', {params: {username: selectedUser.username, role: "ROLE_ADMIN"}});
      if(demoteUserResponse){
        setPromoteDemoteUserModalOpen(false);
        axiosInstance.get("/user/all").then(
          (response) => setUsers(response.data)
        )
      }      
    }

    useEffect(() => {
      let isApiSubscribed = true;
      if(isApiSubscribed){
        axiosInstance.get("/user/all").then(
          (response) => setUsers(response.data)
        )
      }
      return () => {
        isApiSubscribed = false;
      }
    }, [])
    
    if(!user?.roles.some((item) => item.name == "ROLE_ADMIN")){
      return <Navigate to="/"/>
    } 
    else return (
      <Container sx={{padding: "3em", height:"100vh"}}>
        <Modal
          open={addUserModalOpen}
          onClose={() => setAddUserModalOpen(false)}
          aria-labelledby="add-user-modal-title">
            <Box sx={modalStyle}>
                <Typography id="add-user-modal-title" variant="h6">
                  Benutzer hinzufügen
                </Typography>
                <Stack direction="column" spacing={2}>
                  <TextField 
                    label="Benutzername" 
                    variant="standard"
                    value={addUserUsername}
                    onChange={(e) => setAddUserUsername(e.target.value)}
                  />
                  <TextField 
                    label="Passwort" 
                    variant="standard"
                    value={addUserPassword}
                    onChange={(e) => setAddUserPassword(e.target.value)}
                    InputProps={{endAdornment: <IconButton size="small" onClick={() => setAddUserPassword(generateRandomPassword())}><FontAwesomeIcon icon={faRepeat}/></IconButton>}}
                  />
                  <Stack justifyContent="end" direction="row" spacing={2}>
                    <Button variant="contained" color="secondary" sx={{borderRadius:"25px"}} onClick={() => {setAddUserModalOpen(false); setAddUserUsername(""); setAddUserPassword("");}}>
                        Abbrechen
                    </Button>
                    <Button variant="contained" sx={{borderRadius:"25px"}} onClick={() => addUser()}>
                        Hinzufügen
                    </Button>
                  </Stack>
                </Stack>
                
            </Box>
          </Modal>
          <Modal
            open={removeUserModalOpen}
            onClose={() => setRemoveUserModalOpen(false)}
            aria-labelledby="remove-user-modal-title"
            aria-describedby="remove-user-modal-description">
              <Box sx={modalStyle}>
                  <Typography id="remove-user-modal-title" variant="h6">
                    Benutzer entfernen
                  </Typography>
                  <Typography id="remove-user-modal-description" sx={{marginTop: "2em"}}>
                    Sind Sie sich sicher, dass Sie den Benutzer <b>{selectedUser? selectedUser.username : ""}</b> entfernen möchten?
                  </Typography>
                  <Stack justifyContent="end" direction="row" spacing={2} sx={{marginTop: "2em"}}>
                    <Button variant="contained" color="secondary" sx={{borderRadius:"25px"}} onClick={() => setRemoveUserModalOpen(false)}>
                        Abbrechen
                    </Button>
                    <Button variant="contained" color="error" sx={{borderRadius:"25px"}} onClick={() => removeUser()}>
                        Entfernen
                    </Button>
                  </Stack>
              </Box>
            </Modal>
          <Modal
            open={promoteDemoteUserModalOpen}
            onClose={() => setPromoteDemoteUserModalOpen(false)}
            aria-labelledby="promote-demote-user-modal-title"
            aria-describedby="promote-demote-modal-description">
              <Box sx={modalStyle}>
                  <Typography id="promote-demote-modal-title" variant="h6">
                    Benutzer {selectedUser?.roles.some((item) => item.name == "ROLE_ADMIN") ? "herabstufen" : "befördern"}
                  </Typography>
                  <Typography id="promote-demote-modal-description" sx={{marginTop: "2em"}}>
                    Sind Sie sich sicher, dass Sie den Benutzer <b>{selectedUser? selectedUser.username : ""}</b> {selectedUser?.roles.some((item) => item.name == "ROLE_ADMIN") ? "zum Benutzer herabstufen" : "zum Admin befördern"} möchten?
                  </Typography>
                  <Stack justifyContent="end" direction="row" spacing={2} sx={{marginTop: "2em"}}>
                    <Button variant="contained" color="secondary" sx={{borderRadius:"25px"}} onClick={() => setPromoteDemoteUserModalOpen(false)}>
                        Abbrechen
                    </Button>
                    <Button variant="contained" sx={{borderRadius:"25px"}} onClick={() => {if(selectedUser?.roles.some((item) => item.name == "ROLE_ADMIN")){demoteUser()} else {promoteUser()}}}>
                      {selectedUser?.roles.some((item) => item.name == "ROLE_ADMIN") ? "Herabstufen" : "Befördern"}
                    </Button>
                  </Stack>
              </Box>
          </Modal>
        <Header/>
        <Card sx={{marginTop: "2em", borderRadius: "25px", height: "calc(100% - 100px)", overflow: "auto"}}>
          <CardContent sx={{height: "calc(100% - 3em)"}}>
            <Stack direction="column" spacing={2} height={"100%"}>
              <Typography variant="h4"><Box display={"inline"} sx={{"&:hover" : {cursor: "pointer"}}}><FontAwesomeIcon icon={faArrowLeft} onClick={() => navigate(-1)}/></Box> Einstellungen</Typography>              
              <Typography variant="h6">Benutzerverwaltung:</Typography>
              <Card sx={{borderRadius: "25px", backgroundColor: grey[200], flexGrow: 1, overflow: "auto", minHeight: "100px", maxHeight: "100%"}}>
                <CardContent>
                  <List>
                    {
                      users && users.length > 0 ?
                        users.map(
                          (listUser) =>
                            <ListItem key={listUser.username} sx={{backgroundColor: "white", borderRadius:"25px",  marginBottom:"1em"}}
                            secondaryAction={
                              <>
                                <IconButton onClick={() => {setSelectedUser(listUser); setPromoteDemoteUserModalOpen(true);}}
                                  disabled={listUser?.roles.some((item) => item.name == "ROLE_ADMIN") && (users.filter((u) => u?.roles.some((item) => item.name == "ROLE_ADMIN"))).length < 2 || listUser.username == user.username}>
                                  <FontAwesomeIcon icon={faCrown} color={listUser?.roles.some((item) => item.name == "ROLE_ADMIN") ? yellow[600] : undefined}/>
                                </IconButton>
                                <IconButton onClick={() => {setSelectedUser(listUser); setRemoveUserModalOpen(true);}}
                                  disabled={users.length < 2 || (listUser?.roles.some((item) => item.name == "ROLE_ADMIN"))}>
                                  <FontAwesomeIcon icon={faUserMinus}/>
                                </IconButton>
                              </>
                            }>
                              <ListItemText> 
                                <Typography variant="subtitle2">{listUser.username}</Typography> 
                              </ListItemText>
                            </ListItem>
                        )
                      : <></>
                    }
                  </List>
                </CardContent>
              </Card>
              <Stack justifyContent="end" direction="row" spacing={2}>
                <Button variant="contained" sx={{borderRadius:"25px"}} onClick={() => setAddUserModalOpen(true)}>
                    Benutzer hinzufügen
                </Button>
              </Stack>
            </Stack>
          </CardContent>
        </Card>
      </Container>
    )
  }

  export default SettingsView;