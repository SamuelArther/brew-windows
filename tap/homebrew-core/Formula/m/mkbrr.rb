class Mkbrr < Formula
  desc "Is a tool to create, modify and inspect torrent files. Fast"
  homepage "https://mkbrr.com/introduction"
  url "https://github.com/autobrr/mkbrr/archive/refs/tags/v1.27.0.tar.gz"
  sha256 "a9056b74dfe805890a7b629c9aa642297909ee5d8829bbae2594d957ef6c72f1"
  license "GPL-2.0-or-later"
  head "https://github.com/autobrr/mkbrr.git", branch: "develop"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "7febbf5add466257b500f59979088378b8683cb2d4633a1ba1c49424920eccaa"
  end

  depends_on "go" => :build

  deny_network_access!

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args(ldflags: :goreleaser)
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/mkbrr version")

    (testpath/"hello.txt").write "Hello, World!"
    system bin/"mkbrr", "create", (testpath/"hello.txt"), "-o", (testpath/"hello.torrent")

    assert_path_exists testpath/"hello.torrent", "Failed to create torrent file"
  end
end
