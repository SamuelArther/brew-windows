class Nauty < Formula
  desc "Automorphism groups of graphs and digraphs"
  homepage "https://pallini.di.uniroma1.it/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-nauty-2.9.3-1-any.pkg.tar.zst"
  sha256 "22e049c343759ed77a5159cae7aeee8a487bde95f0c906fcd467e81012710d8f"
  license "Apache-2.0"
  version_scheme 1

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url :homepage
    regex(/Current\s+?version:\s*?v?(\d+(?:[._]\d+)+(?:r\d+)?)/i)
    strategy :page_match do |page, regex|
      page.scan(regex).map { |match| match.first.tr("_R", ".r") }
    end
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/NRswitchg.exe", "NRswitchg"
    msys2_bin "ucrt64/bin/addedgeg.exe", "addedgeg"
    msys2_bin "ucrt64/bin/addptg.exe", "addptg"
    msys2_bin "ucrt64/bin/amtog.exe", "amtog"
    msys2_bin "ucrt64/bin/ancestorg.exe", "ancestorg"
    msys2_bin "ucrt64/bin/assembleg.exe", "assembleg"
    msys2_bin "ucrt64/bin/biplabg.exe", "biplabg"
    msys2_bin "ucrt64/bin/catg.exe", "catg"
    msys2_bin "ucrt64/bin/complg.exe", "complg"
    msys2_bin "ucrt64/bin/converseg.exe", "converseg"
    msys2_bin "ucrt64/bin/copyg.exe", "copyg"
    msys2_bin "ucrt64/bin/countg.exe", "countg"
    msys2_bin "ucrt64/bin/countneg.exe", "countneg"
    msys2_bin "ucrt64/bin/cubhamg.exe", "cubhamg"
    msys2_bin "ucrt64/bin/deledgeg.exe", "deledgeg"
    msys2_bin "ucrt64/bin/delptg.exe", "delptg"
    msys2_bin "ucrt64/bin/dimacs2g.exe", "dimacs2g"
    msys2_bin "ucrt64/bin/directg.exe", "directg"
    msys2_bin "ucrt64/bin/distgraphg.exe", "distgraphg"
    msys2_bin "ucrt64/bin/dreadnaut.exe", "dreadnaut"
    msys2_bin "ucrt64/bin/dretodot.exe", "dretodot"
    msys2_bin "ucrt64/bin/dretog.exe", "dretog"
    msys2_bin "ucrt64/bin/edgetransg.exe", "edgetransg"
    msys2_bin "ucrt64/bin/genbg.exe", "genbg"
    msys2_bin "ucrt64/bin/genbgL.exe", "genbgL"
    msys2_bin "ucrt64/bin/geng.exe", "geng"
    msys2_bin "ucrt64/bin/gengL.exe", "gengL"
    msys2_bin "ucrt64/bin/genktreeg.exe", "genktreeg"
    msys2_bin "ucrt64/bin/genposetg.exe", "genposetg"
    msys2_bin "ucrt64/bin/genquarticg.exe", "genquarticg"
    msys2_bin "ucrt64/bin/genrang.exe", "genrang"
    msys2_bin "ucrt64/bin/genspecialg.exe", "genspecialg"
    msys2_bin "ucrt64/bin/gentourng.exe", "gentourng"
    msys2_bin "ucrt64/bin/gentreeg.exe", "gentreeg"
    msys2_bin "ucrt64/bin/hamheuristic.exe", "hamheuristic"
    msys2_bin "ucrt64/bin/labelg.exe", "labelg"
    msys2_bin "ucrt64/bin/linegraphg.exe", "linegraphg"
    msys2_bin "ucrt64/bin/listg.exe", "listg"
    msys2_bin "ucrt64/bin/multig.exe", "multig"
    msys2_bin "ucrt64/bin/nbrhoodg.exe", "nbrhoodg"
    msys2_bin "ucrt64/bin/newedgeg.exe", "newedgeg"
    msys2_bin "ucrt64/bin/pickg.exe", "pickg"
    msys2_bin "ucrt64/bin/planarg.exe", "planarg"
    msys2_bin "ucrt64/bin/productg.exe", "productg"
    msys2_bin "ucrt64/bin/ranlabg.exe", "ranlabg"
    msys2_bin "ucrt64/bin/ransubg.exe", "ransubg"
    msys2_bin "ucrt64/bin/showg.exe", "showg"
    msys2_bin "ucrt64/bin/subdivideg.exe", "subdivideg"
    msys2_bin "ucrt64/bin/twohamg.exe", "twohamg"
    msys2_bin "ucrt64/bin/underlyingg.exe", "underlyingg"
    msys2_bin "ucrt64/bin/uniqg.exe", "uniqg"
    msys2_bin "ucrt64/bin/vcolg.exe", "vcolg"
    msys2_bin "ucrt64/bin/watercluster2.exe", "watercluster2"
  end

  test do
    assert_path_exists bin/"NRswitchg.exe"
  end
end
