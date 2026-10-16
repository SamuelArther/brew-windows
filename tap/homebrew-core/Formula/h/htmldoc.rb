class Htmldoc < Formula
  desc "Convert HTML to PDF or PostScript"
  homepage "https://www.msweet.org/htmldoc/"
  url "https://github.com/michaelrsweet/htmldoc/releases/download/v1.9.24/htmldoc-1.9.24-windows.msi"
  sha256 "60d4369b679acd4d726eb46cf18c40ebba34507a9ba3a08aecdfcf216bc98751"
  license "GPL-2.0-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/michaelrsweet/htmldoc/releases/download/v1.9.24/htmldoc-1.9.24-source.zip"
  end

  def install
    install_tree
    windows_exe "libexec/htmldoc.exe", "htmldoc"
  end

  test do
    system bin/"htmldoc", "--version"
  end
end
