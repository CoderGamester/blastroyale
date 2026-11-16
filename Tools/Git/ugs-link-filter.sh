#!/bin/sh
# Git filter for BlastRoyale/ProjectSettings/ProjectSettings.asset.
# clean: keeps committed cloudProjectId and organizationId blank and projectName as "Blast Royale".
# smudge: restores this clone's local UGS link from `git config ugs.*` on checkout.
case "$1" in
  clean)
    exec sed -E 's/^(  (cloudProjectId|organizationId):).*/\1 /; s/^(  projectName:).*/\1 Blast Royale/'
    ;;
  smudge)
    project="$(git config --get ugs.cloudProjectId || true)"
    organization="$(git config --get ugs.organizationId || true)"
    name="$(git config --get ugs.projectName || echo 'Blast Royale')"
    exec sed -E "s/^(  cloudProjectId:).*/\1 $project/; s/^(  organizationId:).*/\1 $organization/; s/^(  projectName:).*/\1 $name/"
    ;;
  *)
    echo "usage: $0 clean|smudge" >&2
    exit 2
    ;;
esac
