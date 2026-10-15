class Dust < Formula
  desc "More intuitive version of du in rust"
  homepage "https://github.com/bootandy/dust"
  url "https://github.com/bootandy/dust/releases/download/v1.2.6/dust-v1.2.6-x86_64-pc-windows-msvc.zip"
  sha256 "6a73222902d6960ce7183cd833a5d3ea99a3e3ff3cfb33720c1939a41cb7744d"
  license "Apache-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/dust.exe", "dust"
  end

  test do
    assert_match "Dust #{version}", shell_output("#{bin}/dust --version")
  end
end
