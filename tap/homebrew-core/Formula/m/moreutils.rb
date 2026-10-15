class Moreutils < Formula
  desc "Collection of tools that nobody wrote when UNIX was young"
  homepage "https://joeyh.name/code/moreutils/"
  url "https://repo.msys2.org/msys/x86_64/moreutils-0.70-1-x86_64.pkg.tar.zst"
  sha256 "2b620d1de0be826d29ff2e0e19e51ec2645cdc96e7cacc2952066aa6361487f4"
  license all_of: [
    "GPL-2.0-or-later",
    { any_of: ["GPL-2.0-only", "Artistic-2.0"] },
  ]

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url :head
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    msys2_install
    msys2_bin "usr/bin/errno.exe", "errno"
    msys2_bin "usr/bin/ifdata.exe", "ifdata"
    msys2_bin "usr/bin/ifne.exe", "ifne"
    msys2_bin "usr/bin/isutf8.exe", "isutf8"
    msys2_bin "usr/bin/lckdo.exe", "lckdo"
    msys2_bin "usr/bin/mispipe.exe", "mispipe"
    msys2_bin "usr/bin/parallel.exe", "parallel"
    msys2_bin "usr/bin/pee.exe", "pee"
    msys2_bin "usr/bin/sponge.exe", "sponge"
  end

  test do
    system bin/"errno", "--version"
  end
end
