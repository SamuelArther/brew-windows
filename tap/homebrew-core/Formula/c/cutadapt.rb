class Cutadapt < Formula
  include Language::Python::Virtualenv

  desc "Removes adapter sequences from sequencing reads"
  homepage "https://cutadapt.readthedocs.io"
  url "https://github.com/marcelm/cutadapt.git",
      tag:      "v5.2",
      revision: "ef852629f667637439f28761499bb56126e390a1"
  license "MIT"

  bottle do
    sha256 cellar: :any, x86_64_windows: "8e1471a7463153ed0d52269c5963b67a7a410dfee1a6158ac3bb6db68b251743"
  end

  depends_on "python@3.15"

  on_intel do
    depends_on "nasm" => :build
  end

  resource "dnaio" do
    url "https://files.pythonhosted.org/packages/a9/f2/3b4d7f4ac4fc25742fde4c5b50ed1bc60ab731cf8cabd3089dd11e4ea1c9/dnaio-1.2.4.tar.gz"
    sha256 "a7570311f29e8b3c1ea39a60f57b7baf8dad8f2508595c58d4278c5571463166"
  end

  resource "xopen" do
    url "https://files.pythonhosted.org/packages/77/bb/74a5222fc975fccb9f8bfb62613ca42a5a28008362c598df09806ecd12c1/xopen-2.1.0.tar.gz"
    sha256 "06584821588f813563863b63d5e02568a4328661b7507ea38c432f24c69ea4b7"
  end

  def install
    virtualenv_install_with_resources
    pkgshare.install "tests/data"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/cutadapt --version")

    system bin/"cutadapt", "-a", "AACCGGTT", "-o", "output.fastq", pkgshare/"data/small.fastq.gz"
    assert_path_exists "output.fastq"
  end
end
