import {
  HubConnection,
  HubConnectionBuilder,
} from "@microsoft/signalr";

// Plain-JS roster outside the React island (SCOPE closed decision).
// The Room page provides data-session-id on the wrapper element.

interface Participant {
  id: string;
  name: string;
  email: string;
}

const wrapper = document.getElementById("room-wrapper");
const sessionId = wrapper?.dataset.sessionId;

if (sessionId) {
  const rosterList = document.getElementById("roster") as HTMLUListElement;
  const joinButton = document.getElementById("join-btn") as HTMLButtonElement;
  const nameInput = document.getElementById("display-name") as HTMLInputElement;
  const statusEl = document.getElementById("roster-status") as HTMLElement;

  const me: Participant = {
    id: crypto.randomUUID(),
    name: "Guest",
    email: "",
  };

  const inRoster = (id: string) =>
    rosterList.querySelector(`li[data-pid="${CSS.escape(id)}"]`) !== null;

  function addRow(p: Participant) {
    if (inRoster(p.id)) return;
    const li = document.createElement("li");
    li.dataset.pid = p.id;
    li.textContent = p.id === me.id ? `${p.name} (you)` : p.name;
    rosterList.appendChild(li);
  }

  function removeRow(p: Participant) {
    rosterList
      .querySelector(`li[data-pid="${CSS.escape(p.id)}"]`)
      ?.remove();
  }

  function setStatus(text: string) {
    statusEl.textContent = text;
  }

  const connection: HubConnection = new HubConnectionBuilder()
    .withUrl("/hubs/session")
    .withAutomaticReconnect()
    .build();

  // Handlers registered before start so no broadcast is missed.
  connection.on("ParticipantJoined", (p: Participant) => addRow(p));
  connection.on("ParticipantLeft", (p: Participant) => removeRow(p));
  connection.onreconnecting(() => setStatus("Reconnecting..."));
  connection.onreconnected(() => setStatus("Connected"));
  connection.onclose(() => setStatus("Disconnected"));

  joinButton.addEventListener("click", async () => {
    joinButton.disabled = true;
    me.name = nameInput.value.trim() || "Guest";
    setStatus("Connecting...");
    try {
      await connection.start();
      await connection.invoke("JoinSession", sessionId, me);
      // Seed the roster; merge by id since our own join broadcast may
      // have raced this snapshot.
      const current: Participant[] = await connection.invoke(
        "GetParticipants",
        sessionId,
      );
      current.forEach(addRow);
      setStatus("Connected");
      nameInput.disabled = true;
    } catch (err) {
      setStatus(`Failed: ${(err as Error).message}`);
      joinButton.disabled = false;
    }
  });

  window.addEventListener("pagehide", () => {
    // Best effort; the hub's disconnect cleanup is the backstop.
    void connection.invoke("LeaveSession", sessionId).catch(() => {});
  });
}
