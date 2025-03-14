# Telegram WordPress Update Bot

Un bot Telegram qui vous envoie une notification lorsqu'une nouvelle version de WordPress est publiée.

## 🚀 Fonctionnalités

- Vérification automatique des nouvelles versions de WordPress.
- Notification envoyée directement sur Telegram.
- Configuration simple via des variables d'environnement.

## 🛠️ Installation

Voici comment installer et lancer le bot dans un docker.

### 1. Docker

Si vous n'avez pas docker.
Vous pouvez l'installer [ici](https://docs.docker.com/get-started/get-docker/)

### 2. Pull l'image et Lancer le bot
```bash
docker run -d -e envBotToken="yourBotToken" -e envChatId="ChatId" jchaipas/wordpresstelegrambot:latest
```

## 🤝 Contributions
Les contributions sont les bienvenues ! Ouvrez une issue ou un pull request pour proposer des améliorations.

## 📬 Contact
Si vous avez des questions, n’hésitez pas à me contacter via Telegram ou à ouvrir une issue sur GitHub.

---