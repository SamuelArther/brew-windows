class Anyzig < Formula
  desc "Universal zig executable that runs any version of zig"
  homepage "https://github.com/marler8997/anyzig"
  url "https://github.com/marler8997/anyzig/releases/download/v2026_10_13/anyzig-x86_64-windows.zip"
  version "2026"
  sha256 "4e9bf6ab3e602a137c2adf84267940949c495da09ffacaa90bed09842d5a7863"
  license "MIT"

  # Official Windows x64 release binary from the project's GitHub releases.

  livecheck do
    url "https://github.com/marler8997/anyzig/archive/refs/tags/v2026_10_13.tar.gz"
    regex(/^v?(\d+(?:[._]\d+)+)$/i)
    strategy :git do |tags, regex|
      tags.filter_map { |tag| tag[regex, 1]&.tr("_", ".") }
    end
  end

  keg_only "it conflicts with zig"

  def install
    install_tree
    windows_exe "libexec/zig.exe", "zig"
  end

  test do
    assert_match "v#{version.to_s.tr(".", "_")}", shell_output("#{bin}/zig any version").strip
    system bin/"zig", "any", "list-installed"
  end
end
