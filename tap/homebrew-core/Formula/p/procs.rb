class Procs < Formula
  desc "Modern replacement for ps written in Rust"
  homepage "https://github.com/dalance/procs"
  version "0.14.12"
  url "https://github.com/dalance/procs/releases/download/v0.14.12/procs-v0.14.12-x86_64-windows.zip"
  sha256 "4928c399ae78ee82f99139a5629077b8d90a58599d834584dd4e613cb60c83d0"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/procs.exe", "procs"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/procs --version")
  end
end
