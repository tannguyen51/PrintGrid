export interface SampleModel {
  id: string;
  name: string;
  imageUrl: string;
}

export const SAMPLE_MODELS: SampleModel[] = [
  { id: 'sample-1', name: 'Khớp nối bánh răng', imageUrl: 'https://placehold.co/400x300/222/FFF?text=Gear' },
  { id: 'sample-2', name: 'Vỏ hộp Raspberry Pi', imageUrl: 'https://placehold.co/400x300/222/FFF?text=Case' },
  { id: 'sample-3', name: 'Giá đỡ tai nghe', imageUrl: 'https://placehold.co/400x300/222/FFF?text=Stand' },
  { id: 'sample-4', name: 'Mô hình kiến trúc', imageUrl: 'https://placehold.co/400x300/222/FFF?text=Arch' },
];
