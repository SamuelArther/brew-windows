class Bat < Formula
  desc "Clone of cat(1) with syntax highlighting and Git integration"
  homepage "https://github.com/sharkdp/bat"
  version "0.26.1"
  url "https://github.com/sharkdp/bat/releases/download/v0.26.1/bat-v0.26.1-x86_64-pc-windows-msvc.zip"
  sha256 "0f729b4b6f5f28d395c641eacc2e9ff68d0096b85aa0eec344aa62425144b69b"
  license any_of: ["Apache-2.0", "MIT"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bat.exe", "bat"
  end

  test do
    (testpath/"test.txt").write("Hello from bat")
    assert_match "Hello from bat", shell_output("#{bin}/bat --color=never --paging=never test.txt")
  end
end
