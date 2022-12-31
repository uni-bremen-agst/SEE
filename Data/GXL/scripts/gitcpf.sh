#!/usr/bin/env bash

read -p "This script will delete every unversioned file. Are you sure to continue? (y/Y)" -n 1 -r
echo    # (optional) move to a new line
if [[ $REPLY =~ ^[Yy]$ ]]
then
    # path to cpfcsv2rfg
    export PATH=$BAUHAUS/axivion/scripts/:$PATH

    # temporary directory where the GXL will be stored;
    # we clean the checked out revision each time, so that
    # previous results were lost between revisions
    TMPDIR=/tmp/animation-clones-SEE
    mkdir -p "$TMPDIR"

    let i=0
    # iterate on all merge commits into master in reverse chronological order
    # 047ebec392fcea27e3795c18b6d5cb56d0b714be
    for rev in $(git rev-list --merges --first-parent master HEAD | tac)
    do
	let i++
	timestamp=`git show --no-patch --no-notes --pretty=%cd $rev`
	echo "$i" "$rev" "$timestamp"
	# untracked files are removed but modified files are unchanged
	git clean -f -d
	# checkout revision
	git checkout "$rev"
	# detect clones
	cpf -m 100 -C 2 -c clones.cpf -s clones.csv -B $PWD -i "*.cs" \
	    -e "Assets/Animations/*" \
	    -e "Assets/BigFurniturePack/*" \
	    -e "Assets/CScape/*" \
	    -e "Assets/CScapeCDK/*" \
	    -e "Assets/CurvedUI/*" \
	    -e "Assets/DOTween/*" \
	    -e "Assets/InControl/*" \
	    -e "Assets/LeapMotion/*" \
	    -e "Assets/Models/*" \
	    -e "Assets/Modern UI Pack/*" \
	    -e "Assets/OdinSerializer/*" \
	    -e "Assets/Prefabs/*" \
	    -e "Assets/Resources/*" \
	    -e "Assets/SampleScenes/*" \
	    -e "Assets/Scenes/*" \
	    -e "Assets/Standard Assets/*" \
	    -e "Assets/SteamVR/*" \
	    -e "Assets/SteamVR_Input/*" \
	    -e "Assets/SteamVR_Resources/*" \
	    -e "Assets/StreamingAssets/*" \
	    -e "Assets/TextMesh Pro/*" \
	    -e "Assets/Tubular/*" \
	    -e "Assets/Vivox/*" \
	    -e "Assets/waste bin/*" \
	    -e "Assets/WispySky/*" \
	    -e "Assets/XR/*" \
	    Assets
	# generate RFG
	cpfcsv2rfg clones.cpf clones.csv clones.rfg
	# export to GXL
	rfgexport -o Clones -f GXL clones.rfg "$TMPDIR"/clones-"$i".gxl
	# clean up intermediate files
	rm -f clones.cpf clones.csv clones.rfg tokens.files tokens.tok
    done

    # the final destination of the GXL files
    # can be created only now because it would be removed by git clean above
    TARGETDIR=Data/GXL/animation-clones-SEE
    mkdir -p "$TARGETDIR"

    # move resulting GXL files to their final destination
    mv "$TMPDIR"/*.gxl "$TARGETDIR/"
    # clean up
    rm -rf "$TMPDIR"
fi

exit 0
