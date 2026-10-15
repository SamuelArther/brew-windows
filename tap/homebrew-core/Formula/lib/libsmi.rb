class Libsmi < Formula
  desc "Library to Access SMI MIB Information"
  homepage "https://www.ibr.cs.tu-bs.de/projects/libsmi/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libsmi-0.5.0-1-any.pkg.tar.zst"
  sha256 "d432e9f33c36e38e55a90a1b6e1ddfb038b17683ae07b405a5e883db1b549a58"
  license all_of: ["TCL", "BSD-3-Clause", "Beerware"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://www.ibr.cs.tu-bs.de/projects/libsmi/download/"
    regex(/href=.*?libsmi[._-]v?(\d+(?:\.\d+)+)\.t/i)
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/smidiff.exe", "smidiff"
    msys2_bin "ucrt64/bin/smidump.exe", "smidump"
    msys2_bin "ucrt64/bin/smilint.exe", "smilint"
    msys2_bin "ucrt64/bin/smiquery.exe", "smiquery"
    msys2_bin "ucrt64/bin/smixlate.exe", "smixlate"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/smidiff -V")
  end
end
