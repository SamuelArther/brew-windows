class Uru < Formula
  desc "Use multiple rubies on multiple platforms"
  homepage "https://bitbucket.org/jonforums/uru"
  url "https://bitbucket.org/jonforums/uru/get/v0.8.5.tar.gz"
  sha256 "47148454f4c4d5522641ac40aec552a9390a2edc1a0cd306c5d16924f0be7e34"
  license "BSD-3-Clause"

  bottle do
    sha256 cellar: :any, x86_64_windows: "e5acd9fd95e0fed4033d718dad5f64806489907163d065dd9bd196197019fa4f"
  end

  depends_on "go" => :build

  deny_network_access!

  def install
    ENV["GOPATH"] = buildpath
    ENV["GO111MODULE"] = "auto"
    (buildpath/"src/bitbucket.org/jonforums/uru").install Dir["*"]
    system "go", "build", "-ldflags", "-s", "bitbucket.org/jonforums/uru/cmd/uru"
    bin.install "uru" => "uru_rt"
  end

  def caveats
    <<~EOS
      Append to ~/.profile on Ubuntu, or to ~/.zshrc on Zsh
      $ echo 'eval "$(uru_rt admin install)"' >> ~/.bash_profile
    EOS
  end

  test do
    system bin/"uru_rt"
  end
end
