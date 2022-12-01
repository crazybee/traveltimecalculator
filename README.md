# TraveltimeCalculator

This project is used by myself everyday to calculate the travel time from home to work before I start engine. 
It calls either the google map api or the auzre map api to fetch the travel time. 
In the end the travel time message will be forwarded to the azure logic app which will send myself a email containing the travel time. 

Simple Workflow chart

UI &rarr; send request to service bus &rarr; azure function app process the request &rarr; fetch travel time from google api/ azure map api &rarr; logic app &rarr; send myself a email
