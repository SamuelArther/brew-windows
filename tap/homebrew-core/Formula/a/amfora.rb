class Amfora < Formula
  desc "Fancy terminal browser for the Gemini protocol"
  homepage "https://github.com/makew0rld/amfora"
  url "https://github.com/makew0rld/amfora/archive/refs/tags/v1.11.0.tar.gz"
  sha256 "76ae120bdae9a1882acbb2b07a873a52e40265b3ef4c8291de0934c1e9b5982c"
  license all_of: [
    "GPL-3.0-only",
    any_of: ["GPL-3.0-only", "MIT"], # rr
  ]
  head "https://github.com/makew0rld/amfora.git", branch: "master"

  bottle do
    sha256 cellar: :any_skip_relocation, x86_64_windows: "c923b9ef3a9c2afa2a0ee3abc2833fce27fb26f3e9be649acc82d92f7fbb7f19"
  end

  depends_on "go" => :build

  def install
    system "go", "build", *std_go_args(ldflags: :goreleaser)
    pkgshare.install "contrib/themes"
  end

  test do
    # Upstream drives the TUI through `script`, which Windows does not have.
    assert_match version.to_s, shell_output("#{bin}/amfora --version")
  end
end
