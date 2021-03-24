#!/usr/bin/env bash
# for_each_revision.sh first..last

export PATH=$BAUHAUS/axivion/scripts/:$PATH

let i=0
# iterate on all merge commits into master in reverse chronological order
for rev in $(git rev-list --merges --first-parent master HEAD | tac)
do
    let i++
    timestamp=`git show --no-patch --no-notes --pretty=%cd $rev`
    echo "$i" "$rev" "$timestamp"
    git checkout "$rev"
    # detect clones
    cpf -m 100 -C 2 -c clones.cpf -s clones.csv -B $PWD -i Assets/SEE/**.cs .
    # generate RFG
    cpfcsv2rfg clones.cpf clones.csv clones.rfg
    # export to GXL
    rfgexport -o Clones -f GXL clones.rfg clones-"$i".gxl
done

exit 0
