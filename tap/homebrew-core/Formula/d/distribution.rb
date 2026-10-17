class Distribution < Formula
  include Language::Python::Shebang

  desc "Create ASCII graphical histograms in the terminal"
  homepage "https://github.com/time-less-ness/distribution"
  url "https://github.com/time-less-ness/distribution/archive/refs/tags/1.3.tar.gz"
  sha256 "d7f2c9beeee15986d24d8068eb132c0a63c0bd9ace932e724cb38c1e6e54f95d"
  license "GPL-2.0-only"
  head "https://github.com/time-less-ness/distribution.git", branch: "master"

  bottle do
    root_url "https://github.com/SamuelArther/brew-windows/releases/download/distribution-1.3"
    sha256 cellar: :any, x86_64_windows: "95d90a48bd705b646ac3986872b2aa9d4728ecf1595a58243828190557ad5a27"
  end

  uses_from_macos "python"

  def install
    rewrite_shebang detected_python_shebang(use_python_from_path: true), "distribution.py"
    bin.install "distribution.py" => "distribution"
    doc.install "distributionrc", "screenshot.png"
  end

  test do
    assert_match "a|2 (66.67%)", pipe_output("#{bin}/distribution 2>/dev/null", "a\nb\na\n", 0)
  end
end
