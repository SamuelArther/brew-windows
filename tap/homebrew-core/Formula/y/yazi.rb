class Yazi < Formula
  desc "Blazing fast terminal file manager written in Rust, based on async I/O"
  homepage "https://yazi-rs.github.io"
  url "https://github.com/sxyazi/yazi/releases/download/v26.9.1/yazi-x86_64-pc-windows-msvc.zip"
  sha256 "7c033e5f2de6355fd03d865319053fc97db780275974ed17653c057f5d0f8d21"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/yazi.exe", "yazi"
    windows_exe "libexec/ya.exe", "ya"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/yazi --version")
  end
end
