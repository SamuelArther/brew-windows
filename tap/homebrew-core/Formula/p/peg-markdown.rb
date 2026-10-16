class PegMarkdown < Formula
  desc "Markdown implementation based on a PEG grammar"
  homepage "https://github.com/jgm/peg-markdown"
  url "https://github.com/jgm/peg-markdown/archive/refs/tags/0.4.14.tar.gz"
  sha256 "111bc56058cfed11890af11bec7419e2f7ccec6b399bf05f8c55dae0a1712980"
  license any_of: ["GPL-2.0-or-later", "MIT"]
  revision 1
  head "https://github.com/jgm/peg-markdown.git", branch: "master"

  # https://github.com/jgm/peg-markdown/commit/0e0e9e5d7b5e133bd28849e1d72e9636a3b49109
  deprecate! date: "2026-07-16", because: :unmaintained
  disable! date: "2026-10-16", because: :unmaintained

  depends_on "pkgconf" => :build
  depends_on "glib"

  on_macos do
    depends_on "gettext"
  end

  def install
    # Workaround for arm64 linux. Upstream isn't maintained
    ENV.append_to_cflags "-fsigned-char" if OS.linux? && Hardware::CPU.arm?

    system "make"
    bin.install "markdown" => "peg-markdown"
  end

  test do
    assert_equal "<p><strong>Homebrew</strong></p>",
      pipe_output(bin/"peg-markdown", "**Homebrew**", 0).chomp
  end
end
