class Scilla < Formula
  desc "DNS, subdomain, port, directory enumeration tool"
  homepage "https://edoardottt.com/"
  url "https://github.com/edoardottt/scilla/archive/refs/tags/v1.3.4.tar.gz"
  sha256 "f1a738745a2b45aa1dd37e1754a186bc08fb186f01e241257c7ae5a176eb7d44"
  license "GPL-3.0-or-later"
  head "https://github.com/edoardottt/scilla.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "02ddc275aae4d58d605e278563fc1d6e5d58782b80fd639d54d20c867cb1efd6"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/scilla"
  end

  test do
    output = shell_output("#{bin}/scilla dns -target brew.sh")
    assert_match <<~EOS, output
      =====================================================
      target: brew.sh
      ================ SCANNING DNS =======================
    EOS

    assert_match version.to_s, shell_output("#{bin}/scilla --help 2>&1", 1)
  end
end
