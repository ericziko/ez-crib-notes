---
uid: 5cf814a2-c6c6-4d6a-9e15-c396fdf96c85
---


# question-about-hashicorp-vault-structure


## 🤖❓ Please create a markdown document answering the following question

### IMPORTANT 
  - DO NOT LEAVE THE CURRENT DIRECTORY 
  - Ask any relevant questions from me if you need clarification. 

### Question

On my project at work, we are using HashiCorp Vault, and we have a main project with a bunch of submodules that do various different things, all broken up into microservices. Per environment, the microservices all share the same connection strings, yet we keep creating subtrees in HashiCorp Vault for each microservice, duplicating connection string information all over the place, which is further multiplied by the number of environments. I think this is an anti-pattern, but I don't know. Do we need to break them up on that granular a level, or is there a better strategy?



