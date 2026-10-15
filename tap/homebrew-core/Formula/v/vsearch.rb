class Vsearch < Formula
  desc "Versatile open-source tool for microbiome analysis"
  homepage "https://github.com/torognes/vsearch"
  url "https://github.com/torognes/vsearch/releases/download/v2.32.0/vsearch-2.32.0-win-x86_64.zip"
  sha256 "50ead8474607a325c9021bae890f10d0fa296489d5c266a4dc621e23b98637cf"
  license any_of: ["BSD-2-Clause", "GPL-3.0-or-later"]

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/torognes/vsearch/archive/refs/tags/v2.32.0.tar.gz"
  end

  def install
    install_tree
    windows_exe "libexec/bin/vsearch.exe", "vsearch"
  end

  test do
    (testpath/"test.fasta").write <<~FASTA
      >U00096.2:1-70
      AGCTTTTCATTCTGACTGCAACGGGCAATATGTCTCTGTGTGGATTAAAAAAAGAGTGTCTGATAGCAGC
    FASTA
    system bin/"vsearch", "--rereplicate", "test.fasta", "--output", "output.txt"
    assert_path_exists testpath/"output.txt"
  end
end
