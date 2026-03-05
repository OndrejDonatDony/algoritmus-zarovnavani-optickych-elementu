clear all;

fileID = fopen('odkaz.txt','r');
textdata = textscan(fileID,'%s');
fclose(fileID);
fileNames = string(textdata{:});
numFiles = size(fileNames, 1);

out = regexprep(fileNames, '^C:\\Users\\ondre\\Desktop\\BAKALÁŘKA\\BakalarniPrace\\data\\', '');

for i = 1:numFiles
    img = imread(fileNames{i});

    det = ChunkDetector(img);
    BW = det.BinaryImg();

    figure;

    subplot(1,2,1);
    imshow(BW, []);
    title(out(i), 'Interpreter', 'none');
    xlabel('x [px]'); ylabel('y [px]');

    subplot(1,2,2);
    imshow(det.G, []);
    title(out(i), 'Interpreter', 'none');
    xlabel('x [px]'); ylabel('y [px]');
end
