class KertishDos < Formula
  desc "Kertish Object Storage and Cluster Administration CLI"
  homepage "https://github.com/freakmaxi/kertish-dos"
  url "https://github.com/freakmaxi/kertish-dos/archive/refs/tags/v22.2.0147.tar.gz"
  version "22.2.0147-532592"
  sha256 "fe76b525762a3240e8c4bc8e6d7caedebf466aec81c1a22f8014d6881c2bdaf6"
  license "GPL-3.0-only"
  head "https://github.com/freakmaxi/kertish-dos.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "808e6e86d90c9ad2b079da0e251b10f2f5ed80f176372e756b29132de34cc97e"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download", "-C", "fs-tool"
    system "go", "mod", "download", "-C", "admin-tool"
  end

  def install
    cd "fs-tool" do
      system "go", "build", *std_go_args(output: bin/"krtfs", ldflags: "-X main.version=#{version}")
    end
    cd "admin-tool" do
      system "go", "build", *std_go_args(output: bin/"krtadm", ldflags: "-X main.version=#{version}")
    end
  end

  test do
    port = free_port
    assert_match("failed.\nlocalhost:#{port}: head node is not reachable",
      shell_output("#{bin}/krtfs -t localhost:#{port} ls"))
    assert_match("localhost:#{port}: manager node is not reachable",
      shell_output("#{bin}/krtadm -t localhost:#{port} -get-clusters", 70))
  end
end
