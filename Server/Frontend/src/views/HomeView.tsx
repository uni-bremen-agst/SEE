import {Button, Container } from "@mui/material";
import Header from "../components/Header";
import ServerList from "../components/ServerList";
import { useNavigate } from "react-router";

function HomeView() {
  const navigate = useNavigate();

    return (
      <Container sx={{padding: "3em", height:"100vh"}}>
        <Header/>
        <ServerList/>
        <Button onClick={() => navigate('/createServer')} sx={{width:"100%", borderRadius: "15px"}} variant="contained">Hinzufügen</Button>
      </Container>
    )
  }

  export default HomeView;