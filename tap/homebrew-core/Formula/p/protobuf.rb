class Protobuf < Formula
  desc "Protocol buffers (Google's data interchange format)"
  homepage "https://protobuf.dev/"
  url "https://github.com/protocolbuffers/protobuf/releases/download/v36.2/protoc-36.2-win64.zip"
  sha256 "f0c128dc0d8492eceece83bb459a4c0e316764b929ffbf1aa416357fd644edd3"
  license "BSD-3-Clause"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    install_tree
    windows_exe "libexec/bin/protoc.exe", "protoc"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/protoc --version")
  end
end
