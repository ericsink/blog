#!/bin/sh
aws s3 sync wwwroot s3://ericsink.com --delete --acl public-read --dryrun

