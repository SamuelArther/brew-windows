class Pwgen < Formula
  desc "Password generator"
  homepage "https://pwgen.sourceforge.net/"
  url "https://repo.msys2.org/msys/x86_64/pwgen-2.08-2-x86_64.pkg.tar.zst"
  sha256 "c8eab94af3a0d4cc96cb4bf262c3f19908d5414d8aadbcb51caab0d4c0d81bc9"
  license "GPL-2.0-only"

  # Prebuilt package from MSYS2 (an isolated root per keg; MSYS2 path rules apply).

  livecheck do
    url "https://downloads.sourceforge.net/project/pwgen/pwgen/2.08/pwgen-2.08.tar.gz"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/pwgen.exe", "pwgen"
  end

  test do
    system bin/"pwgen", "--secure", "20", "10"
  end
end
