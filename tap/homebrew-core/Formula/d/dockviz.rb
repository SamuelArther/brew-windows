class Dockviz < Formula
  desc "Visualizing docker data"
  homepage "https://github.com/justone/dockviz"
  url "https://github.com/justone/dockviz/archive/refs/tags/v0.6.4.tar.gz"
  sha256 "228e2c11fad1de38ec63e5d46e60bf9411625447c3c381dd0cd99aaa05d4e5db"
  license "Apache-2.0"
  head "https://github.com/justone/dockviz.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "48f552edeb461643044b3022826ad7716d53e4fad3f7a46d4234762cb86aa7fa"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/dockviz --version")
  end
end
