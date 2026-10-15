# Upstream has published no Windows binaries since v12.1.2 (the v13 and v15 releases
# have no assets), so this uses the newest official Windows x64 build.
class Tokei < Formula
  desc "Program that allows you to count code, quickly"
  homepage "https://github.com/XAMPPRocky/tokei"
  version "12.1.2"
  url "https://github.com/XAMPPRocky/tokei/releases/download/v12.1.2/tokei-x86_64-pc-windows-msvc.exe"
  sha256 "b1d6c4b18f5fa238bd2c6e47caa65a7a3e4a1bd0de6df0b7c19c8083c941f57b"
  license any_of: ["Apache-2.0", "MIT"]

  livecheck do
    url :stable
    strategy :github_latest
  end

  def install
    bin.install "tokei-x86_64-pc-windows-msvc.exe" => "tokei.exe"
  end

  test do
    (testpath/"lib.rs").write <<~RUST
      #[cfg(test)]
      mod tests {
          #[test]
          fn test() {
              println!("It works!");
          }
      }
    RUST
    system bin/"tokei", "lib.rs"
  end
end
