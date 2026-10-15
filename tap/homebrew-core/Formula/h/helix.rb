class Helix < Formula
  desc "Post-modern modal text editor"
  homepage "https://helix-editor.com"
  url "https://github.com/helix-editor/helix/releases/download/25.07.1/helix-25.07.1-x86_64-windows.zip"
  sha256 "5c8325ced8bacd8418d62706f669e96d9c3578a9237526e34d546900cbc049b6"
  license "MPL-2.0"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/hx.exe", "hx"
  end

  test do
    assert_match "helix #{version}", shell_output("#{bin}/hx --version")
  end
end
