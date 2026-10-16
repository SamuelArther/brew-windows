class Feishu2md < Formula
  desc "Convert feishu/larksuite documents to markdown"
  homepage "https://github.com/Wsine/feishu2md"
  url "https://github.com/Wsine/feishu2md/releases/download/v2.4.5/feishu2md-v2.4.5-windows-amd64.zip"
  sha256 "3f200ac8af42b1191cb092de79de0289edc3259d77b656dedc4ef421c36f441d"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/Wsine/feishu2md/archive/refs/tags/v2.4.5.tar.gz"
    strategy :github_latest
  end

  def install
    bin.install "feishu2md.exe"
  end

  test do
    output = shell_output("#{bin}/feishu2md config --appId testAppId --appSecret testSecret")
    assert_match "testAppId", output

    assert_match version.to_s, shell_output("#{bin}/feishu2md --version")
  end
end
