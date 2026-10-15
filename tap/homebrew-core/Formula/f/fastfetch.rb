class Fastfetch < Formula
  desc "Like neofetch, but much faster because written mostly in C"
  homepage "https://github.com/fastfetch-cli/fastfetch"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-fastfetch-2.69.0-1-any.pkg.tar.zst"
  sha256 "83a2b0269031268f2ce7f01fdcbaa801cf3cb533b0de4348af9df51defe4a988"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/fastfetch-cli/fastfetch/archive/refs/tags/2.70.0.tar.gz"
    strategy :github_latest
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/fastfetch.exe", "fastfetch"
  end

  test do
    assert_match "fastfetch", shell_output("#{bin}/fastfetch --version")
    assert_match "OS", shell_output("#{bin}/fastfetch --structure OS --pipe")
  end
end
