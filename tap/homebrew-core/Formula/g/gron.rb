class Gron < Formula
  desc "Make JSON greppable"
  homepage "https://github.com/tomnomnom/gron"
  url "https://github.com/tomnomnom/gron/releases/download/v0.7.1/gron-windows-amd64-0.7.1.zip"
  sha256 "5ed427a4a504d8e03a1770b71d4ad16a3764179e085b5ae84e51a57b299f300d"
  license "MIT"

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "gron.exe"
  end

  test do
    assert_match "json.a = 1;", pipe_output("#{bin}/gron", "{\"a\": 1}")
  end
end
