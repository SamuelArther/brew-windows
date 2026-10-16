class Zfind < Formula
  desc "Search for files (even inside tar/zip/7z/rar) using a SQL-WHERE filter"
  homepage "https://github.com/laktak/zfind"
  url "https://github.com/laktak/zfind/archive/refs/tags/v0.4.7.tar.gz"
  sha256 "49bc01da8446c8a97182f9794032d851614f0efc75b4f4810a114491a08d3bd4"
  license "MIT"
  head "https://github.com/laktak/zfind.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7e940594e6f9ffbd7396857c61f2d51b7957e22bb9a1deb734f4731680f3e0d2"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: "-X main.appVersion=#{version}"), "./cmd/zfind"
  end

  test do
    output = shell_output("#{bin}/zfind --csv")
    assert_match "name,path,container,size,date,time,ext,ext2,type,archive", output

    assert_match version.to_s, shell_output("#{bin}/zfind --version")
  end
end
