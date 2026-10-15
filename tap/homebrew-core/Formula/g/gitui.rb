class Gitui < Formula
  desc "Blazing fast terminal-ui for git written in rust"
  homepage "https://github.com/gitui-org/gitui"
  url "https://github.com/gitui-org/gitui/releases/download/v0.28.1/gitui-win.tar.gz"
  sha256 "fab116ccdf9ae4e17378db912d76cc26e60aefd2226f44e665120dea166867e4"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "gitui.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/gitui --version")
  end
end
