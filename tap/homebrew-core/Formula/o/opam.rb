class Opam < Formula
  desc "OCaml package manager"
  homepage "https://opam.ocaml.org"
  url "https://github.com/ocaml/opam/releases/download/2.6.1/opam-2.6.1-x86_64-windows.exe"
  sha256 "2548f0592abfa728432c85ef6d08bf97c295c1859ddca61876e33884336c130c"
  license "LGPL-2.1-only"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/ocaml/opam/releases/download/2.6.1/opam-full-2.6.1.tar.gz"
    regex(/^opam-full[._-]v?(\d+(?:[.-]\d+)+)\.t/i)
    strategy :github_latest do |json, regex|
      json["assets"]&.map do |asset|
        match = asset["name"]&.match(regex)
        next if match.blank?

        match[1]
      end
    end
  end

  def install
    bin.install "opam-2.6.1-x86_64-windows.exe" => "opam.exe"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/opam --version 2>&1")
  end
end
