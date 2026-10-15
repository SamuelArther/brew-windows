class Argon2 < Formula
  desc "Password hashing library and CLI utility"
  homepage "https://github.com/P-H-C/phc-winner-argon2"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-argon2-20190702-2-any.pkg.tar.zst"
  version "20190702"
  sha256 "6ba6498f06f78768c83b9378df2fc75346f5654386b3c323104e0bec0c5eaa70"
  license any_of: ["CC0-1.0", "Apache-2.0"]

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/P-H-C/phc-winner-argon2/archive/refs/tags/20190702.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/argon2.exe", "argon2"
  end

  test do
    output = pipe_output("#{bin}/argon2 somesalt -t 2 -m 16 -p 4", "password", 0)
    assert_match "c29tZXNhbHQ$IMit9qkFULCMA/ViizL57cnTLOa5DiVM9eMwpAvPw", output
  end
end
