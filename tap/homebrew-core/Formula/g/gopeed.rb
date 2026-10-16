class Gopeed < Formula
  desc "Multi protocol download manager"
  homepage "https://gopeed.com"
  url "https://github.com/GopeedLab/gopeed/archive/refs/tags/v1.9.3.tar.gz"
  sha256 "da8da9516fbe2a01db1dab11dda97c7e3096e43a14a7c13202a2f92976aa6db9"
  license "GPL-3.0-or-later"
  head "https://github.com/GopeedLab/gopeed.git", branch: "main"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "0cddbb0162e3a9aaccba3ee4a73f3bf97b50a9c0a98abb28d9eebe3036488b8c"
  end

  depends_on "go" => :build

  allow_network_access! :test

  def fetch
    system "go", "mod", "download"
  end

  def install
    system "go", "build", *std_go_args, "./cmd/gopeed"
  end

  test do
    output = shell_output("#{bin}/gopeed https://example.com/")
    assert_match "saving path: #{testpath}", output
    assert_match "Example Domain", (testpath/"example.com").read
  end
end
