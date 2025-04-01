# ATARI Ops

This repositry regroups the configuration-as-code to provision, configure,
deploy and manage the EPFL's WPTelegram bot. It uses [Ansible] wrapped in a
convenient [suitcase], called [`wpbotsible`](./wpbotsible).

All the "dev" code can be found in https://github.com/Jch4ipas/TelegrambotWP.


## Prerequisites

* Access to `itsidevfsd0013` VMs.


## TL;DR

`./wpbotsible`

install Docker, Install docker image, clone the code, manage secrets, run the containers.



[Ansible]: https://www.ansible.com (Ansible is Simple IT Automation)
[suitcase]: https://github.com/epfl-si/ansible.suitcase (Install Ansible and its dependency stack into a temporary directory)
[github]: https://github.com/Jch4ipas/TelegrambotWP
[//]: # "comment"
