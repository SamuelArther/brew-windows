class Grpcurl < Formula
  desc "Like cURL, but for gRPC"
  homepage "https://www.fullstory.com/resources/content/fullstory-engineering-blog/"
  url "https://github.com/fullstorydev/grpcurl/releases/download/v1.9.4/grpcurl_1.9.4_windows_x86_64.zip"
  sha256 "412dbe98dc6f3fddfbdc15db061768495d3b4c6ee24de998c83c36f79857680d"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "grpcurl.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/grpcurl -version 2>&1")
  end
end
