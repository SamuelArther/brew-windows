class Kakoune < Formula
  desc "Selection-based modal text editor"
  homepage "https://github.com/mawww/kakoune"
  url "https://github.com/mawww/kakoune/releases/download/v2026.05.21/kakoune-2026.05.21.tar.bz2"
  sha256 "be1deb3fe9808a0733ab1057309da380bb757307e8fdbb22dc478b674b6bad34"
  license "Unlicense"
  head "https://github.com/mawww/kakoune.git", branch: "master"

  livecheck do
    url :stable
    strategy :github_latest
  end

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/kakoune-2026.05.21"
    sha256 cellar: :any_skip_relocation, x86_64_windows: "adf7d20ea602c0b8638fc6c9991ae1ebec3bd873515b93e8d1c9000363693fc5"
  end

  on_linux do
    depends_on "binutils" => :build
  end

  fails_with :clang do
    build 1200
    cause "Requires C++20"
  end

  # See <https://github.com/mawww/kakoune/blob/v2022.10.31/README.asciidoc#building>
  fails_with :gcc do
    version "10.2"
    cause "Requires GCC >= 10.3"
  end

  def install
    system "make", "install", "debug=no", "PREFIX=#{prefix}"
  end

  test do
    system bin/"kak", "-ui", "dummy", "-e", "q"

    assert_match version.to_s, shell_output("#{bin}/kak -version")
  end
end
